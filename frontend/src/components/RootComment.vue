<script setup lang="ts">
import { computed, ref } from 'vue'
import type { CommentDto, RootCommentDto } from '@/api/types'
import CommentForm from '@/components/CommentForm.vue'
import CommentMeta from '@/components/CommentMeta.vue'
import CommentNode from '@/components/CommentNode.vue'
import { renderCommentHtml } from '@/utils/sanitize'
import type { PendingReplies } from '@/composables/useRealtime'
import { buildReplyTree } from '@/utils/tree'

const props = defineProps<{ comment: RootCommentDto; pending?: PendingReplies }>()

const emit = defineEmits<{
  created: [comment: CommentDto]
  loadReplies: []
  refresh: []
}>()

const replying = ref(false)
const tree = computed(() => buildReplyTree(props.comment.id, props.comment.replies))

function onCreated(comment: CommentDto) {
  replying.value = false
  emit('created', comment)
}
</script>

<template>
  <article class="root-comment">
    <CommentMeta
      :user-name="comment.userName"
      :email="comment.email"
      :home-page="comment.homePage"
      :created-at="comment.createdAt"
    />
    <div class="comment-text" v-html="renderCommentHtml(comment.text)" />

    <div v-if="pending" class="pending-replies">
      <button v-if="pending.extra === 0" type="button" class="btn" @click="emit('loadReplies')">+{{ pending.ids.length }} ответов. Показать</button>
      <button v-else type="button" class="btn" @click="emit('refresh')">Новых ответов много. Обновить страницу</button>
    </div>

    <button v-if="!replying" type="button" class="btn link" @click="replying = true">Ответить</button>
    <CommentForm v-else :parent-id="comment.id" @created="onCreated" @cancel="replying = false" />

    <div v-if="tree.length" class="replies">
      <CommentNode v-for="node in tree" :key="node.comment.id" :node="node" :depth="1" @created="emit('created', $event)" />
    </div>
  </article>
</template>
